import { makeCrud } from '../../shared/ui/useCrudResource';
import { vehicleSchema } from './vehicleTypes';
import type { Vehicle, VehicleForm } from './vehicleTypes';

export const vehiclesCrud = makeCrud<Vehicle, VehicleForm, VehicleForm>({
  key: 'vehicles',
  basePath: '/api/v1/vehicles',
  itemSchema: vehicleSchema,
  getId: (v) => v.id,
});
