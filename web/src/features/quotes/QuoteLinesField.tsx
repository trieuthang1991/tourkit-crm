import { Button, Select } from 'antd';
import { useMemo } from 'react';
import { useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { NumberField, SelectField, TextField } from '../../shared/ui/Field';
import { useAllProviderPrices } from './quotesApi';
import { SCOPE_OPTIONS, SERVICE_TYPE_OPTIONS } from './types';

/// Editor dòng dự trù giá (spec 2026-07-11): loại DV + phạm vi (đoàn/khách) + giá vốn từ bảng giá NCC
/// (chọn tự điền, sửa được) + %LN. Giá bán = vốn × (1+%LN) tính ở server; vốn = 0 → gõ giá bán tay.
export function QuoteLinesField() {
  const { control, setValue } = useFormContext();
  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });
  const prices = useAllProviderPrices();
  const lines = useWatch({ control, name: 'lines' }) as Array<{ providerServiceId?: string | null }> | undefined;

  const priceOptions = useMemo(
    () =>
      (prices.data ?? []).map((s) => ({
        label:
          s.currencyCode && s.currencyCode !== 'VND'
            ? `${s.priceName ?? 'Giá'} — ${s.contractPrice.toLocaleString('vi-VN')} ${s.currencyCode} (${s.contractPriceVnd.toLocaleString('vi-VN')}₫)`
            : `${s.priceName ?? 'Giá'} — ${s.contractPriceVnd.toLocaleString('vi-VN')}₫`,
        value: s.id,
      })),
    [prices.data],
  );

  return (
    <div>
      {fields.map((field, index) => (
        <div key={field.id} style={{ borderBottom: '1px dashed #ddd', paddingBottom: 8, marginBottom: 8 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
            <div style={{ flex: 2 }}>
              <TextField name={`lines.${index}.description`} label={index === 0 ? 'Mô tả' : ' '} required />
            </div>
            <div style={{ width: 140 }}>
              <SelectField name={`lines.${index}.serviceType`} label={index === 0 ? 'Loại DV' : ' '} options={SERVICE_TYPE_OPTIONS} />
            </div>
            <div style={{ width: 130 }}>
              <SelectField name={`lines.${index}.scope`} label={index === 0 ? 'Phạm vi' : ' '} options={SCOPE_OPTIONS} />
            </div>
            <Button danger style={{ marginTop: index === 0 ? 30 : 4 }} onClick={() => remove(index)}>
              Xoá
            </Button>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
            <div style={{ flex: 2 }}>
              {/* Chọn bảng giá NCC → tự điền giá vốn (vẫn sửa tay được). */}
              <Select
                style={{ width: '100%' }}
                placeholder="Chọn từ bảng giá NCC (tuỳ chọn)"
                allowClear
                loading={prices.isLoading}
                options={priceOptions}
                value={lines?.[index]?.providerServiceId ?? undefined}
                onChange={(v) => {
                  const picked = (prices.data ?? []).find((s) => s.id === v);
                  setValue(`lines.${index}.providerServiceId`, v ?? null);
                  if (picked) {
                    // Lưu giá vốn VND (đã quy đổi theo tỷ giá).
                    setValue(`lines.${index}.unitCost`, picked.contractPriceVnd);
                  }
                }}
              />
            </div>
            <div style={{ width: 90 }}>
              <NumberField name={`lines.${index}.quantity`} label={index === 0 ? 'SL' : ' '} required />
            </div>
            <div style={{ width: 130 }}>
              <NumberField name={`lines.${index}.unitCost`} label={index === 0 ? 'Giá vốn' : ' '} />
            </div>
            <div style={{ width: 90 }}>
              <NumberField name={`lines.${index}.marginPercent`} label={index === 0 ? '%LN' : ' '} />
            </div>
            <div style={{ width: 130 }}>
              <NumberField name={`lines.${index}.unitPrice`} label={index === 0 ? 'Giá bán (vốn=0)' : ' '} />
            </div>
          </div>
        </div>
      ))}
      <Button
        type="dashed"
        block
        onClick={() =>
          append({
            description: '',
            quantity: 1,
            unitPrice: 0,
            serviceType: 0,
            scope: 1,
            providerServiceId: null,
            unitCost: 0,
            marginPercent: 0,
          })
        }
      >
        + Thêm dòng
      </Button>
    </div>
  );
}
