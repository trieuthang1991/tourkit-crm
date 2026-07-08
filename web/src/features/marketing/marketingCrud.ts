import { makeCrud } from '../../shared/ui/useCrudResource';
import { campaignSchema } from './types';
import type { Campaign, CampaignForm } from './types';

export const marketingCrud = makeCrud<Campaign, CampaignForm, CampaignForm>({
  key: 'campaigns',
  basePath: '/api/v1/marketing/campaigns',
  itemSchema: campaignSchema,
  getId: (c) => c.id,
});
