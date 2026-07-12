import { Typography } from 'antd';
import { DepartureCalendar } from '../booking/DepartureCalendar';

export function OperationsCalendarPage() {
  return (
    <>
      <Typography.Title level={3}>Lịch điều hành</Typography.Title>
      <DepartureCalendar />
    </>
  );
}
