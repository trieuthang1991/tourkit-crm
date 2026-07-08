import { App, Button, Card, Col, Descriptions, Row, Space, Tag, Typography } from 'antd';
import { errorMessage } from '../../shared/api/problem';
import { dateText, money, statusText } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { useChangePlan, usePlans, useSubscription } from './billingApi';
import { SUBSCRIPTION_STATUS } from './billingTypes';
import type { Plan } from './billingTypes';

export function BillingPage() {
  const { has } = useAuth();
  const { message } = App.useApp();
  const subscription = useSubscription();
  const plans = usePlans();
  const changePlan = useChangePlan();

  const canManage = has('subscription.manage');

  async function choosePlan(plan: Plan) {
    try {
      await changePlan.mutateAsync({ planCode: plan.code });
      message.success(`Đã chuyển sang gói ${plan.name}`);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <Typography.Title level={3}>Gói dịch vụ</Typography.Title>
      <Card title="Gói hiện tại" loading={subscription.isLoading} style={{ marginBottom: 16 }}>
        {subscription.data ? (
          <Descriptions column={2}>
            <Descriptions.Item label="Mã gói">{subscription.data.planCode}</Descriptions.Item>
            <Descriptions.Item label="Trạng thái">
              {statusText(SUBSCRIPTION_STATUS, subscription.data.status)}
            </Descriptions.Item>
            <Descriptions.Item label="Bắt đầu">{dateText(subscription.data.startedAt)}</Descriptions.Item>
            <Descriptions.Item label="Hết hạn">{dateText(subscription.data.expiresAt)}</Descriptions.Item>
          </Descriptions>
        ) : null}
      </Card>
      <Row gutter={16}>
        {(plans.data ?? []).map((plan) => {
          const isCurrent = subscription.data?.planCode === plan.code;
          return (
            <Col span={8} key={plan.id} style={{ marginBottom: 16 }}>
              <Card title={plan.name} extra={isCurrent ? <Tag color="green">Đang dùng</Tag> : null}>
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div>Mã gói: {plan.code}</div>
                  <div>Số người dùng tối đa: {plan.maxUsers}</div>
                  <div>Số tour tối đa: {plan.maxTours}</div>
                  <div>Giá / tháng: {money(plan.priceMonthly)}</div>
                  {canManage ? (
                    <Button
                      type={isCurrent ? 'default' : 'primary'}
                      disabled={isCurrent}
                      loading={changePlan.isPending}
                      onClick={() => choosePlan(plan)}
                    >
                      Chọn gói
                    </Button>
                  ) : null}
                </Space>
              </Card>
            </Col>
          );
        })}
      </Row>
    </>
  );
}
