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
