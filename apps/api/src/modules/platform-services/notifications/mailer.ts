import { Injectable, Logger } from '@nestjs/common';
import { env } from '@erp/config';

/**
 * Mail port — TARGET_ARCHITECTURE §8 ("email (SMTP/SES port)").
 *
 * PHASE_04 §4 scopes this to "an interface + console/mailhog stub": no provider SDK, no
 * templates, no retries here. Delivery is queued through `outbox_jobs` → the
 * `notifications` queue, so the transport can be swapped without touching a caller.
 */

export type MailMessage = {
  to: string;
  subject: string;
  text: string;
  /** Tenant the message belongs to; used for per-tenant templates later. */
  tenantId?: string | null;
};

export interface MailerPort {
  send(message: MailMessage): Promise<void>;
  readonly transport: string;
}

export const MAILER = 'ERP_MAILER';

/**
 * The wired implementation. In development `MAIL_TRANSPORT=console` prints the message;
 * MailHog (compose service `mailhog`, SMTP 1025) is the target once an SMTP transport is
 * implemented in a later phase — the port is what keeps that a one-file change.
 */
@Injectable()
export class ConsoleMailer implements MailerPort {
  private readonly logger = new Logger(ConsoleMailer.name);
  readonly transport = 'console';

  async send(message: MailMessage): Promise<void> {
    this.logger.log(
      {
        transport: this.transport,
        from: env.MAIL_FROM,
        to: message.to,
        subject: message.subject,
        tenantId: message.tenantId ?? null,
      },
      'outbound mail (console transport)',
    );
  }
}
