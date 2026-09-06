import { createHash, randomBytes } from 'node:crypto';

import { Injectable } from '@nestjs/common';
import { SignJWT, importPKCS8, importSPKI, jwtVerify, type JWTPayload, type KeyLike } from 'jose';
import { env, normalisePem } from '@erp/config';
import { DomainError, errorCodes, newId } from '@erp/contracts';

/**
 * Token issuance & verification.
 *
 * Access token: JWT **RS256**, 15 min, claims `{ sub, tid, mid, scope }` + `jti`
 * (PROJECT_CONTRACT §9, SECURITY_ARCHITECTURE §2).
 * Refresh token: opaque 256-bit random string, stored **only** as a SHA-256 hash,
 * 30 days, rotating with reuse detection → family revocation.
 */

export type AccessTokenClaims = {
  sub: string;
  tid: string;
  mid: string;
  scope: string[];
  jti: string;
  pam?: boolean;
};

const ISSUER = 'erp-saas';
const AUDIENCE = 'erp-saas-api';

@Injectable()
export class TokenService {
  private signingKey: KeyLike | undefined;
  private verificationKey: KeyLike | undefined;

  private async privateKey(): Promise<KeyLike> {
    if (!this.signingKey) {
      const pem = normalisePem(env.JWT_PRIVATE_KEY);
      if (!pem) {
        throw new DomainError(errorCodes.INTERNAL, 'JWT_PRIVATE_KEY is not configured', 500);
      }
      this.signingKey = await importPKCS8(pem, 'RS256');
    }
    return this.signingKey;
  }

  private async publicKey(): Promise<KeyLike> {
    if (!this.verificationKey) {
      const pem = normalisePem(env.JWT_PUBLIC_KEY);
      if (!pem) {
        throw new DomainError(errorCodes.INTERNAL, 'JWT_PUBLIC_KEY is not configured', 500);
      }
      this.verificationKey = await importSPKI(pem, 'RS256');
    }
    return this.verificationKey;
  }

  get accessTtlSeconds(): number {
    return env.JWT_ACCESS_TTL_SECONDS;
  }

  get refreshTtlSeconds(): number {
    return env.JWT_REFRESH_TTL_SECONDS;
  }

  async signAccessToken(claims: Omit<AccessTokenClaims, 'jti'>): Promise<{ token: string; jti: string }> {
    const jti = newId();
    const token = await new SignJWT({
      scope: claims.scope,
      tid: claims.tid,
      mid: claims.mid,
      ...(claims.pam ? { pam: true } : {}),
    })
      .setProtectedHeader({ alg: 'RS256', typ: 'JWT', kid: env.JWT_KEY_ID })
      .setSubject(claims.sub)
      .setIssuedAt()
      .setIssuer(ISSUER)
      .setAudience(AUDIENCE)
      .setJti(jti)
      .setExpirationTime(`${this.accessTtlSeconds}s`)
      .setNotBefore('0s')
      .sign(await this.privateKey());

    return { token, jti };
  }

  /** Throws `UNAUTHENTICATED` for any verification failure — never leaks the reason. */
  async verifyAccessToken(token: string): Promise<AccessTokenClaims> {
    let payload: JWTPayload;
    try {
      const result = await jwtVerify(token, await this.publicKey(), {
        issuer: ISSUER,
        audience: AUDIENCE,
        algorithms: ['RS256'],
      });
      payload = result.payload;
    } catch {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Access token is invalid or expired', 401);
    }

    const sub = payload.sub;
    const tid = payload.tid;
    const mid = payload.mid;
    const jti = payload.jti;
    const scope = payload.scope;

    if (
      typeof sub !== 'string' ||
      typeof tid !== 'string' ||
      typeof mid !== 'string' ||
      typeof jti !== 'string' ||
      !Array.isArray(scope)
    ) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Access token is missing required claims', 401);
    }

    return {
      sub,
      tid,
      mid,
      jti,
      scope: scope.filter((entry): entry is string => typeof entry === 'string'),
      pam: payload.pam === true,
    };
  }

  /** Opaque refresh token: 256 bits of randomness, base64url. */
  generateRefreshToken(): string {
    return randomBytes(32).toString('base64url');
  }

  /** SHA-256 at rest — the plaintext never touches the database or the logs. */
  hashRefreshToken(token: string): string {
    return createHash('sha256').update(token).digest('hex');
  }

  /** Signs a token for a tenant the caller is *not* a member of — test helper. */
  async signForgedAccessToken(claims: Omit<AccessTokenClaims, 'jti'>): Promise<string> {
    return (await this.signAccessToken(claims)).token;
  }
}
