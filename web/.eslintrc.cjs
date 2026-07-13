module.exports = {
  root: true,
  env: { browser: true, es2021: true },
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react-hooks/recommended',
  ],
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 'latest',
    sourceType: 'module',
    project: './tsconfig.json',
  },
  plugins: ['@typescript-eslint', 'react-hooks', 'react-refresh'],
  ignorePatterns: ['dist', '.eslintrc.cjs', 'vite.config.ts', 'vitest.config.ts'],
  rules: {
    '@typescript-eslint/no-explicit-any': 'error',
    'react-hooks/rules-of-hooks': 'error',
    'react-hooks/exhaustive-deps': 'error',
    'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
  },
  overrides: [
    {
      // Chỉ chặn import chéo GIỮA các feature (vd: features/tourTemplates
      // import thẳng file nội bộ của features/customers). Lớp app/ được
      // phép import mọi feature để lắp ráp router/providers — đó là vai trò
      // của nó, không phải import chéo feature.
      files: ['src/features/**/*.{ts,tsx}'],
      rules: {
        'no-restricted-imports': [
          'error',
          {
            paths: [
              {
                name: 'antd',
                message:
                  "Không import 'antd' trực tiếp trong feature. Dùng component custom từ '@/shared/ui', hoặc re-export '@/shared/ui/antd' cho primitive (Input/Select/…).",
              },
            ],
            patterns: [
              {
                group: ['../../features/*/**', '../../../features/*/**'],
                message:
                  'Không import trực tiếp file nội bộ của feature khác. Nâng lên shared/ nếu cần dùng chung.',
              },
              {
                group: ['antd/*'],
                message: "Không import 'antd/*' trực tiếp trong feature. Dùng re-export '@/shared/ui/antd'.",
              },
            ],
          },
        ],
      },
    },
  ],
};
