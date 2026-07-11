import { z } from 'zod';
import { workTaskSchema } from '../workTasks/types';

export const workflowSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  startDate: z.string().nullable(),
  endDate: z.string().nullable(),
  status: z.number(),
  sectionCount: z.number(),
  taskCount: z.number(),
});
export type Workflow = z.infer<typeof workflowSchema>;

export const workflowCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  startDate: z.string().nullable(),
  endDate: z.string().nullable(),
});
export type WorkflowCreateForm = z.infer<typeof workflowCreateSchema>;

export const workflowSectionSchema = z.object({
  id: z.string().uuid(),
  workflowId: z.string().uuid(),
  name: z.string(),
  sort: z.number(),
  color: z.string().nullable(),
  icon: z.string().nullable(),
  allowUpdate: z.boolean(),
  allowDelete: z.boolean(),
});
export type WorkflowSection = z.infer<typeof workflowSectionSchema>;

export const boardColumnSchema = z.object({
  section: workflowSectionSchema,
  tasks: z.array(workTaskSchema),
});
export type BoardColumn = z.infer<typeof boardColumnSchema>;

export const workflowBoardSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  status: z.number(),
  columns: z.array(boardColumnSchema),
});
export type WorkflowBoard = z.infer<typeof workflowBoardSchema>;
