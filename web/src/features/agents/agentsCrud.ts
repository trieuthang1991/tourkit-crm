import { makeCrud } from '../../shared/ui/useCrudResource';
import { agentSchema } from './types';
import type { Agent, AgentForm } from './types';

export const agentsCrud = makeCrud<Agent, AgentForm, AgentForm>({
  key: 'agents',
  basePath: '/api/v1/agents',
  itemSchema: agentSchema,
  getId: (a) => a.id,
});
