import { Button } from 'antd';
import { useFieldArray, useFormContext } from 'react-hook-form';
import { NumberField, TextField } from '../../shared/ui/Field';

/// <summary>Editor dòng báo giá động — thêm/xoá dòng, tái dùng Field với tên lồng lines.i.x (react-hook-form).</summary>
export function QuoteLinesField() {
  const { control } = useFormContext();
  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  return (
    <div>
      {fields.map((field, index) => (
        <div key={field.id} style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
          <div style={{ flex: 2 }}>
            <TextField name={`lines.${index}.description`} label={index === 0 ? 'Mô tả' : ' '} required />
          </div>
          <div style={{ width: 90 }}>
            <NumberField name={`lines.${index}.quantity`} label={index === 0 ? 'SL' : ' '} required />
          </div>
          <div style={{ width: 150 }}>
            <NumberField name={`lines.${index}.unitPrice`} label={index === 0 ? 'Đơn giá' : ' '} required />
          </div>
          <Button danger style={{ marginTop: index === 0 ? 30 : 4 }} onClick={() => remove(index)}>
            Xoá
          </Button>
        </div>
      ))}
      <Button type="dashed" block onClick={() => append({ description: '', quantity: 1, unitPrice: 0 })}>
        + Thêm dòng
      </Button>
    </div>
  );
}
