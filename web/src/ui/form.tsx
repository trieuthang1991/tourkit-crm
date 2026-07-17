import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { Controller, FormProvider, useForm, useFormContext } from 'react-hook-form';
import type { DefaultValues, FieldValues } from 'react-hook-form';
import type { ZodType } from 'zod';
import { Button } from './kit';
import { DateInput, Input, NumberInput, Select, Textarea } from './inputs';
import type { Option } from './inputs';
import { Drawer } from './overlay';

function Labeled({ label, required, error, children }: { label: string; required?: boolean; error?: string; children: ReactNode }) {
  return (
    <div className="mb-4">
      <label className="mb-1.5 block text-[12.5px] font-medium text-[#5e5873]">
        {label} {required ? <span className="text-[#d1494a]">*</span> : null}
      </label>
      {children}
      {error ? <div className="mt-1.5 text-[12px] text-[#d1494a]">{error}</div> : null}
    </div>
  );
}

const lc = (label: string) => label.replace(/\s*\(.*?\)\s*/g, '').trim().toLowerCase();

export function TextField({ name, label, required, placeholder }: { name: string; label: string; required?: boolean; placeholder?: string }) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Labeled label={label} required={required} error={formState.errors[name]?.message as string}>
          <Input value={(field.value as string) ?? ''} onChange={field.onChange} placeholder={placeholder ?? `Nhập ${lc(label)}`} />
        </Labeled>
      )}
    />
  );
}

export function NumberField({ name, label, required, placeholder }: { name: string; label: string; required?: boolean; placeholder?: string }) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Labeled label={label} required={required} error={formState.errors[name]?.message as string}>
          <NumberInput value={(field.value as number) ?? null} onChange={field.onChange} placeholder={placeholder} />
        </Labeled>
      )}
    />
  );
}

export function TextAreaField({ name, label, required, rows, placeholder }: { name: string; label: string; required?: boolean; rows?: number; placeholder?: string }) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Labeled label={label} required={required} error={formState.errors[name]?.message as string}>
          <Textarea value={(field.value as string) ?? ''} onChange={field.onChange} rows={rows} placeholder={placeholder ?? `Nhập ${lc(label)}`} />
        </Labeled>
      )}
    />
  );
}

export function DatePickerField({ name, label, required }: { name: string; label: string; required?: boolean; placeholder?: string }) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Labeled label={label} required={required} error={formState.errors[name]?.message as string}>
          <DateInput value={(field.value as string) ?? null} onChange={field.onChange} />
        </Labeled>
      )}
    />
  );
}

export function SelectField({
  name,
  label,
  options,
  required,
  allowClear,
  showSearch,
  mode,
  placeholder,
}: {
  name: string;
  label: string;
  options: Option[];
  required?: boolean;
  allowClear?: boolean;
  showSearch?: boolean;
  placeholder?: string;
  /** tags = multi + cho phép tự nhập giá trị mới (giữ hành vi mode="tags" của AntD) */
  mode?: 'multiple' | 'tags';
}) {
  const { control, formState } = useFormContext();
  const tags = mode === 'tags';
  const multiple = mode === 'multiple' || tags;
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Labeled label={label} required={required} error={formState.errors[name]?.message as string}>
          <Select
            value={field.value ?? (multiple ? [] : null)}
            onChange={(v) => field.onChange(v ?? (multiple ? [] : null))}
            options={options}
            allowClear={allowClear}
            showSearch={showSearch || multiple}
            multiple={multiple}
            tags={tags}
            placeholder={placeholder ?? `${tags ? 'Chọn hoặc nhập' : 'Chọn'} ${lc(label)}`}
          />
        </Labeled>
      )}
    />
  );
}

export function CheckboxField({ name, label }: { name: string; label: string }) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <label className="mb-4 flex cursor-pointer items-center gap-2 text-[14px] text-[#5e5873]">
          <input type="checkbox" checked={!!field.value} onChange={(e) => field.onChange(e.target.checked)} className="h-4 w-4 accent-[#eb5324]" />
          {label}
        </label>
      )}
    />
  );
}

/** CrudDrawer — form trong Drawer trượt phải. CÙNG API với shared/ui/CrudFormModal để migrate dễ. */
export function CrudDrawer<T extends FieldValues>({
  open,
  title,
  schema,
  defaultValues,
  submitting,
  onCancel,
  onSubmit,
  width = 560,
  children,
}: {
  open: boolean;
  title: ReactNode;
  schema: ZodType<T>;
  defaultValues: DefaultValues<T>;
  submitting?: boolean;
  onCancel: () => void;
  onSubmit: (v: T) => void;
  width?: number;
  children: ReactNode;
}) {
  const methods = useForm<T>({ resolver: zodResolver(schema), defaultValues });
  useEffect(() => {
    if (open) methods.reset(defaultValues);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <Drawer
      open={open}
      title={title}
      onClose={onCancel}
      width={width}
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
          <Button onClick={onCancel}>Huỷ</Button>
          <Button variant="primary" onClick={methods.handleSubmit(onSubmit)} disabled={submitting}>
            {submitting ? 'Đang lưu…' : 'Lưu'}
          </Button>
        </div>
      }
    >
      <FormProvider {...methods}>
        <form onSubmit={methods.handleSubmit(onSubmit)}>{children}</form>
      </FormProvider>
    </Drawer>
  );
}
