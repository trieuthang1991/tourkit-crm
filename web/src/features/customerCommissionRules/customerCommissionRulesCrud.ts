import { makeCrud } from '../../shared/ui/useCrudResource';
import { customerCommissionRuleSchema } from './types';
import type { CustomerCommissionRule, CustomerCommissionRuleForm } from './types';

export const customerCommissionRulesCrud = makeCrud<
  CustomerCommissionRule,
  CustomerCommissionRuleForm,
  CustomerCommissionRuleForm
>({
  key: 'customerCommissionRules',
  basePath: '/api/v1/customer-commission-rules',
  itemSchema: customerCommissionRuleSchema,
  getId: (r) => r.id,
});
