import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../../shared/api/httpClient';
import { tourTemplateSchema } from '../types';

// ---- Single template (header) ----

export function useTourTemplate(id: string) {
  return useQuery({
    queryKey: ['tourTemplates', id],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/tour-templates/${id}`);
      return tourTemplateSchema.parse(data);
    },
    enabled: !!id,
  });
}

// ---- Itinerary ----

const itineraryDaySchema = z.object({
  id: z.string(),
  dayIndex: z.number(),
  title: z.string(),
  detail: z.string().nullable(),
});
export type ItineraryDay = z.infer<typeof itineraryDaySchema>;
export type ItineraryDayInput = { dayIndex: number; title: string; detail: string | null };

const itineraryKey = (templateId: string) => ['tourTemplates', templateId, 'itinerary'] as const;
const itineraryPath = (templateId: string) => `/api/v1/tour-templates/${templateId}/itinerary`;

export function useItinerary(templateId: string) {
  return useQuery({
    queryKey: itineraryKey(templateId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(itineraryPath(templateId));
      return z.array(itineraryDaySchema).parse(data);
    },
    enabled: !!templateId,
  });
}

export function usePutItinerary(templateId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: ItineraryDayInput[]) => {
      await httpClient.put(itineraryPath(templateId), body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: itineraryKey(templateId) }),
  });
}

// ---- Price scenarios ----

const priceScenarioSchema = z.object({
  id: z.string(),
  fromQty: z.number(),
  toQty: z.number(),
  unitPrice: z.number(),
});
export type PriceScenario = z.infer<typeof priceScenarioSchema>;
export type PriceScenarioInput = { fromQty: number; toQty: number; unitPrice: number };

const priceScenariosKey = (templateId: string) => ['tourTemplates', templateId, 'price-scenarios'] as const;
const priceScenariosPath = (templateId: string) => `/api/v1/tour-templates/${templateId}/price-scenarios`;

export function usePriceScenarios(templateId: string) {
  return useQuery({
    queryKey: priceScenariosKey(templateId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(priceScenariosPath(templateId));
      return z.array(priceScenarioSchema).parse(data);
    },
    enabled: !!templateId,
  });
}

export function usePutPriceScenarios(templateId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: PriceScenarioInput[]) => {
      await httpClient.put(priceScenariosPath(templateId), body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: priceScenariosKey(templateId) }),
  });
}

// ---- Assignees (tourId == template id) ----

const assigneeSchema = z.object({
  id: z.string(),
  userId: z.string().uuid(),
  role: z.number(),
});
export type Assignee = z.infer<typeof assigneeSchema>;
export type AssigneeInput = { userId: string; role: number };

export const ASSIGNEE_ROLE: Record<number, string> = {
  1: 'Quản lý',
  2: 'Theo dõi',
  3: 'Phụ trách',
};

const assigneesKey = (tourId: string) => ['tours', tourId, 'assignees'] as const;
const assigneesPath = (tourId: string) => `/api/v1/tours/${tourId}/assignees`;

export function useAssignees(tourId: string) {
  return useQuery({
    queryKey: assigneesKey(tourId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(assigneesPath(tourId));
      return z.array(assigneeSchema).parse(data);
    },
    enabled: !!tourId,
  });
}

export function usePutAssignees(tourId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: AssigneeInput[]) => {
      await httpClient.put(assigneesPath(tourId), body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: assigneesKey(tourId) }),
  });
}
