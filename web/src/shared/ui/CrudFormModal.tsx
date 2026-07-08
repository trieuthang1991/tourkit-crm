import { Form, Modal } from 'antd';
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
};

export function CrudFormModal<T extends FieldValues>({
  open,
  title,
  schema,
  defaultValues,
  submitting,
  onCancel,
  onSubmit,
  children,
}: CrudFormModalProps<T>) {
  const methods = useForm<T>({ resolver: zodResolver(schema), defaultValues });
  useEffect(() => {
    if (open) {
      methods.reset(defaultValues);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <Modal
      open={open}
      title={title}
      onCancel={onCancel}
      onOk={methods.handleSubmit(onSubmit)}
      confirmLoading={submitting}
      destroyOnHidden
    >
      <FormProvider {...methods}>
        <Form layout="vertical">{children}</Form>
      </FormProvider>
    </Modal>
  );
}
