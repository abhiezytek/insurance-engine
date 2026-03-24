export const AUDIT_TYPES = {
  payoutVerification: 'PayoutVerification',
  additionBonus: 'AdditionBonus',
} as const;

export type AuditTypeKey = keyof typeof AUDIT_TYPES;

export const AUDIT_ROUTE = {
  search: '/api/audit/search',
  approve: '/api/audit/approve',
  reject: '/api/audit/reject',
  bulkDecision: '/api/audit/bulk-decision',
  template: '/api/audit/template',
  upload: '/api/audit/upload',
  cases: '/api/audit/cases',
  batches: '/api/audit/batches',
  logs: '/api/audit/logs',
} as const;

export const PAYOUT_ROUTE = {
  search: '/api/payout/search',
  checkerApprove: '/api/payout/checker/approve',
  checkerReject: '/api/payout/checker/reject',
  authorizerApprove: '/api/payout/authorizer/approve',
  authorizerReject: '/api/payout/authorizer/reject',
  bulkCheckerApprove: '/api/payout/checker/bulk-approve',
  bulkAuthorizerApprove: '/api/payout/authorizer/bulk-approve',
  cases: '/api/payout/cases',
  batches: '/api/payout/batches',
  dashboard: '/api/payout/dashboard',
  batchGenerate: '/api/payout/batch/generate',
  upload: '/api/payout/upload',
  template: '/api/payout/template',
  export: '/api/payout/export',
} as const;

export const AUDIT_LABEL = {
  payoutVerification: 'Payout Verification',
  additionBonus: 'Addition / Bonus',
} as const;

export function auditTypeLabel(key: 'payout-verification' | 'addition-bonus') {
  return key === 'payout-verification' ? AUDIT_TYPES.payoutVerification : AUDIT_TYPES.additionBonus;
}

export function auditSubtitle(key: 'payout-verification' | 'addition-bonus') {
  return key === 'payout-verification' ? AUDIT_LABEL.payoutVerification : AUDIT_LABEL.additionBonus;
}
