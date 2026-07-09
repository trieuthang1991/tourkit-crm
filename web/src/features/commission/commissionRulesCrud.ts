import { makeCrud } from '../../shared/ui/useCrudResource';
import { commissionRuleSchema } from './commissionRuleTypes';
import type { CommissionRule, CommissionRuleForm } from './commissionRuleTypes';

export const commissionRulesCrud = makeCrud<CommissionRule, CommissionRuleForm, CommissionRuleForm>({
  key: 'commission-rules',
  basePath: '/api/v1/commission-rules',
  itemSchema: commissionRuleSchema,
  getId: (r) => r.id,
});
