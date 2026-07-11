import { makeCrud } from '../../shared/ui/useCrudResource';
import { guideAssignmentSchema } from './guideAssignmentTypes';
import type { GuideAssignment, GuideAssignmentForm } from './guideAssignmentTypes';

export const guideAssignmentsCrud = makeCrud<GuideAssignment, GuideAssignmentForm, GuideAssignmentForm>({
  key: 'guideAssignments',
  basePath: '/api/v1/guide-assignments',
  itemSchema: guideAssignmentSchema,
  getId: (g) => g.id,
});
