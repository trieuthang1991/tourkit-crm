import { makeCrud } from '../../shared/ui/useCrudResource';
import { vehicleAssignmentSchema } from './vehicleAssignmentTypes';
import type { VehicleAssignment, VehicleAssignmentForm } from './vehicleAssignmentTypes';

export const vehicleAssignmentsCrud = makeCrud<VehicleAssignment, VehicleAssignmentForm, VehicleAssignmentForm>({
  key: 'vehicleAssignments',
  basePath: '/api/v1/vehicle-assignments',
  itemSchema: vehicleAssignmentSchema,
  getId: (v) => v.id,
});
