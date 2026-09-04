import js from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import tsParser from '@typescript-eslint/parser';
import importPlugin from 'eslint-plugin-import';
import boundaries from 'eslint-plugin-boundaries';
import globals from 'globals';

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
          selector: 'Identifier[name=/^(price|amount|total|balance|cost|rate)$/i]',
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
