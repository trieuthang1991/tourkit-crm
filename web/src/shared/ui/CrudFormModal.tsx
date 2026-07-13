import { Button, Drawer, Form, Space } from 'antd';
import { zodResolver } from '@hookform/resolvers/zod';
import { FormProvider, useForm } from 'react-hook-form';
import type { DefaultValues, FieldValues } from 'react-hook-form';
import type { ReactNode } from 'react';
import { useEffect } from 'react';
import type { z } from 'zod';

type CrudFormModalProps<T extends FieldValues> = {
  open: boolean;
  title: string;
  schema: z.ZodType<T>;
  defaultValues: DefaultValues<T>;
  submitting: boolean;
  onCancel: () => void;
  onSubmit: (values: T) => void;
  children: ReactNode;
  /** Bề rộng drawer (px). Mặc định 560; form nhiều cột có thể truyền lớn hơn. */
  width?: number;
};

// Panel trượt từ phải thay cho modal giữa màn: diện tích rộng, dễ thao tác form dài.
export function CrudFormModal<T extends FieldValues>({
  open,
  title,
  schema,
  defaultValues,
  submitting,
  onCancel,
  onSubmit,
  children,
  width = 560,
}: CrudFormModalProps<T>) {
  const methods = useForm<T>({ resolver: zodResolver(schema), defaultValues });
  useEffect(() => {
    if (open) {
      methods.reset(defaultValues);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <Drawer
      open={open}
      title={title}
      placement="right"
      onClose={onCancel}
      destroyOnHidden
      maskClosable={!submitting}
      // Rộng nhưng không tràn trên màn nhỏ.
      width={`min(${width}px, 96vw)`}
      styles={{ body: { paddingBottom: 24 } }}
      footer={
        <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <Button onClick={onCancel} disabled={submitting}>
            Huỷ
          </Button>
          <Button type="primary" loading={submitting} onClick={methods.handleSubmit(onSubmit)}>
            Lưu
          </Button>
        </Space>
      }
    >
      <FormProvider {...methods}>
        <Form layout="vertical" onFinish={methods.handleSubmit(onSubmit)}>
          {children}
        </Form>
      </FormProvider>
    </Drawer>
  );
}
