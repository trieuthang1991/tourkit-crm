import { makeCrud } from '../../shared/ui/useCrudResource';
import { ticketFundSchema } from './types';
import type { TicketFund, TicketFundForm } from './types';

export const ticketFundsCrud = makeCrud<TicketFund, TicketFundForm, TicketFundForm>({
  key: 'ticketFunds',
  basePath: '/api/v1/ticket-funds',
  itemSchema: ticketFundSchema,
  getId: (t) => t.id,
});
