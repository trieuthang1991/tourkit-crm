import { describe, expect, it } from 'vitest';
import { workflowBoardSchema, workflowSchema } from './types';

describe('workflow schemas', () => {
  it('parses a board summary', () => {
    const w = workflowSchema.parse({
      id: crypto.randomUUID(),
      name: 'Điều hành tour hè',
      startDate: null,
      endDate: null,
      status: 0,
      sectionCount: 3,
      taskCount: 5,
    });
    expect(w.name).toBe('Điều hành tour hè');
    expect(w.sectionCount).toBe(3);
  });

  it('parses a board detail with columns and cards', () => {
    const sectionId = crypto.randomUUID();
    const board = workflowBoardSchema.parse({
      id: crypto.randomUUID(),
      name: 'Board',
      status: 0,
      columns: [
        {
          section: {
            id: sectionId,
            workflowId: crypto.randomUUID(),
            name: 'Cần làm',
            sort: 0,
            color: null,
            icon: null,
            allowUpdate: true,
            allowDelete: true,
          },
          tasks: [
            {
              id: crypto.randomUUID(),
              title: 'Đặt xe',
              description: null,
              assigneeUserId: null,
              assigneeName: null,
              dueDate: null,
              priority: 1,
              status: 0,
              relatedOrderId: null,
              workflowId: crypto.randomUUID(),
              sectionId,
            },
          ],
        },
      ],
    });
    expect(board.columns).toHaveLength(1);
    const col = board.columns[0]!;
    expect(col.tasks[0]!.title).toBe('Đặt xe');
    expect(col.section.name).toBe('Cần làm');
  });
});
