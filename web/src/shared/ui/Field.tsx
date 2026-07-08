import { Form, Input, InputNumber, Select } from 'antd';
import { Controller, useFormContext } from 'react-hook-form';

type Option = { label: string; value: number | string };

export function TextField({ name, label, required }: { name: string; label: string; required?: boolean }) {
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
          <Input {...field} value={field.value ?? ''} />
        </Form.Item>
      )}
    />
  );
}

export function NumberField({ name, label, required }: { name: string; label: string; required?: boolean }) {
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
          <InputNumber style={{ width: '100%' }} value={field.value} onChange={field.onChange} />
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
}: {
  name: string;
  label: string;
  options: Option[];
  required?: boolean;
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
          <Select {...field} options={options} />
        </Form.Item>
      )}
    />
  );
}
