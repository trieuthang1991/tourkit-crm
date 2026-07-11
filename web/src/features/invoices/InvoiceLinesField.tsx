import { Button } from 'antd';
import { useFieldArray, useFormContext } from 'react-hook-form';
import { NumberField, TextField } from '../../shared/ui/Field';

/// <summary>Editor dòng hoá đơn động — thêm/xoá dòng (Description/SL/Đơn giá/Thuế suất %) qua useFieldArray.</summary>
export function InvoiceLinesField() {
  const { control } = useFormContext();
  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  return (
    <div>
      {fields.map((field, index) => (
        <div key={field.id} style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
          <div style={{ flex: 2 }}>
            <TextField name={`lines.${index}.description`} label={index === 0 ? 'Mô tả' : ' '} required />
          </div>
          <div style={{ width: 80 }}>
            <NumberField name={`lines.${index}.quantity`} label={index === 0 ? 'SL' : ' '} required />
          </div>
          <div style={{ width: 130 }}>
            <NumberField name={`lines.${index}.unitPrice`} label={index === 0 ? 'Đơn giá' : ' '} required />
          </div>
          <div style={{ width: 90 }}>
            <NumberField name={`lines.${index}.vatRate`} label={index === 0 ? 'Thuế %' : ' '} required />
          </div>
          <Button danger style={{ marginTop: index === 0 ? 30 : 4 }} onClick={() => remove(index)}>
            Xoá
          </Button>
        </div>
      ))}
      <Button type="dashed" block onClick={() => append({ description: '', quantity: 1, unitPrice: 0, vatRate: 10 })}>
        + Thêm dòng
      </Button>
    </div>
  );
}
