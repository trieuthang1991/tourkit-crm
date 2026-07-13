import { Checkbox, DatePicker, Form, Input, InputNumber, Select } from 'antd';
import dayjs from 'dayjs';
import { Controller, useFormContext } from 'react-hook-form';

const { TextArea } = Input;

type Option = { label: string; value: number | string };

// Placeholder mặc định từ label (tránh input trống không gợi ý). Bỏ phần chú thích trong ngoặc.
const lc = (label: string) => label.replace(/\s*\(.*?\)\s*/g, '').trim().toLowerCase();
const fillPh = (label: string, ph?: string) => ph ?? `Nhập ${lc(label)}`;
const pickPh = (label: string, ph?: string) => ph ?? `Chọn ${lc(label)}`;

export function CheckboxField({ name, label }: { name: string; label: string }) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Item>
          <Checkbox checked={!!field.value} onChange={(e) => field.onChange(e.target.checked)}>
            {label}
          </Checkbox>
        </Form.Item>
      )}
    />
  );
}

export function TextField({ name, label, required, placeholder }: { name: string; label: string; required?: boolean; placeholder?: string }) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Item
          label={label}
          required={required}
          validateStatus={formState.errors[name] ? 'error' : ''}
          help={formState.errors[name]?.message as string | undefined}
        >
          <Input {...field} value={field.value ?? ''} placeholder={fillPh(label, placeholder)} />
        </Form.Item>
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
        <Form.Item
          label={label}
          required={required}
          validateStatus={formState.errors[name] ? 'error' : ''}
          help={formState.errors[name]?.message as string | undefined}
        >
          <InputNumber style={{ width: '100%' }} value={field.value} onChange={field.onChange} placeholder={placeholder ?? '0'} />
        </Form.Item>
      )}
    />
  );
}

export function TextAreaField({
  name,
  label,
  required,
  rows = 4,
  placeholder,
}: {
  name: string;
  label: string;
  required?: boolean;
  rows?: number;
  placeholder?: string;
}) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Item
          label={label}
          required={required}
          validateStatus={formState.errors[name] ? 'error' : ''}
          help={formState.errors[name]?.message as string | undefined}
        >
          <TextArea {...field} value={field.value ?? ''} rows={rows} placeholder={fillPh(label, placeholder)} />
        </Form.Item>
      )}
    />
  );
}

export function DatePickerField({ name, label, required, placeholder }: { name: string; label: string; required?: boolean; placeholder?: string }) {
  const { control, formState } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Item
          label={label}
          required={required}
          validateStatus={formState.errors[name] ? 'error' : ''}
          help={formState.errors[name]?.message as string | undefined}
        >
          <DatePicker
            style={{ width: '100%' }}
            format="DD/MM/YYYY"
            placeholder={placeholder ?? 'dd/mm/yyyy'}
            value={field.value ? dayjs(field.value as string) : null}
            onChange={(date) => field.onChange(date ? date.toISOString() : null)}
          />
        </Form.Item>
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
  mode?: 'multiple' | 'tags';
  placeholder?: string;
}) {
  const { control, formState } = useFormContext();
  const multi = mode === 'multiple' || mode === 'tags';
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Item
          label={label}
          required={required}
          validateStatus={formState.errors[name] ? 'error' : ''}
          help={formState.errors[name]?.message as string | undefined}
        >
          <Select
            {...field}
            mode={mode}
            value={field.value ?? (multi ? [] : undefined)}
            options={options}
            placeholder={pickPh(label, placeholder)}
            allowClear={allowClear}
            showSearch={showSearch || multi}
            optionFilterProp={showSearch || multi ? 'label' : undefined}
            onChange={(v) => field.onChange(v ?? (multi ? [] : null))}
          />
        </Form.Item>
      )}
    />
  );
}
