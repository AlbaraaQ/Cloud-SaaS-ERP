import js from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import tsParser from '@typescript-eslint/parser';
import boundaries from 'eslint-plugin-boundaries';
import importPlugin from 'eslint-plugin-import';
import globals from 'globals';

/**
 * Money guard (PROJECT_CONTRACT §3: "IEEE `number` is forbidden for money").
 *
 * The Phase-01 selector matched *every* `Identifier` node with a money-like name, which
 * also matched object-literal keys and member-access property names. That made the
 * frozen list envelope of API_CONTRACT §0 / API_ARCHITECTURE §3
 * (`{ data, meta: { total, limit, offset } }`) impossible to write or read without a
 * lint error. The selector below keeps the guard on every *value position*
 * (declarations, parameters, type annotations of variables) and stops matching pure
 * field names, which carry no numeric type at all.
 * Recorded in docs/change-log/CHANGE-REQUESTS.md (CR-001, non-structural).
 */
const MONEY_IDENTIFIER = '/^(price|amount|total|balance|cost|rate)$/i';
const MONEY_SELECTOR = [
  `Identifier[name=${MONEY_IDENTIFIER}]`,
  ':not(Property > Identifier.key)',
  ':not(MemberExpression > Identifier.property)',
  ':not(TSPropertySignature > Identifier.key)',
  ':not(TSMethodSignature > Identifier.key)',
].join('');

export default [
  {
    ignores: ['**/dist/**', '**/.next/**', '**/node_modules/**', '**/coverage/**'],
  },
  js.configs.recommended,
  {
    files: ['**/*.{ts,tsx,mts,mjs,js}'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
      },
      globals: {
        ...globals.node,
      },
    },
    settings: {
      'boundaries/elements': [
        { type: 'app', pattern: ['apps/**/*.ts', 'apps/**/*.js', 'apps/**/*.mjs'] },
        { type: 'lib', pattern: ['packages/**/*.ts', 'packages/**/*.js', 'packages/**/*.mjs'] },
      ],
    },
    plugins: {
      '@typescript-eslint': tseslint,
      import: importPlugin,
      boundaries,
    },
    rules: {
      'no-restricted-syntax': [
        'error',
        {
          selector: MONEY_SELECTOR,
          message: 'Use decimal.js / string money types; avoid number for money-like identifiers.',
        },
      ],
      'import/order': [
        'error',
        {
          groups: ['builtin', 'external', 'internal', 'parent', 'sibling', 'index'],
          'newlines-between': 'always',
        },
      ],
      'boundaries/element-types': [
        'error',
        {
          default: 'disallow',
          rules: [
            {
              from: ['app'],
              allow: ['app', 'lib'],
            },
            {
              from: ['lib'],
              allow: ['lib'],
            },
          ],
        },
      ],
      // The base rule does not understand TypeScript parameter properties
      // (`constructor(public readonly status: number)`), so the typed variant replaces it.
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
  {
    files: ['**/*.d.ts'],
    rules: {
      'spaced-comment': 'off',
    },
  },
];
