import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { AppShell } from './AppShell';
import { TourTemplateListPage } from '../features/tourTemplates/TourTemplateListPage';
import { TourTemplateDetailPage } from '../features/tourTemplates/TourTemplateDetailPage';
import { CustomersPage } from '../features/customers/CustomersPage';
import { LeadsPage } from '../features/leads/LeadsPage';
import { ProvidersPage } from '../features/providers/ProvidersPage';
import { MarketingPage } from '../features/marketing/MarketingPage';
import { MarketTypesPage } from '../features/marketTypes/MarketTypesPage';
import { DeparturesPage } from '../features/booking/DeparturesPage';
import { DepartureDetailPage } from '../features/booking/DepartureDetailPage';
import { OrdersPage } from '../features/booking/OrdersPage';
import { OrderDetailPage } from '../features/booking/OrderDetailPage';
import { BillingPage } from '../features/billing/BillingPage';
import { OrderDebtReportPage } from '../features/reports/OrderDebtReportPage';

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<Navigate to="/customers" replace />} />
        <Route path="/customers" element={<CustomersPage />} />
        <Route path="/leads" element={<LeadsPage />} />
        <Route path="/providers" element={<ProvidersPage />} />
        <Route path="/marketing" element={<MarketingPage />} />
        <Route path="/market-types" element={<MarketTypesPage />} />
        <Route path="/departures" element={<DeparturesPage />} />
        <Route path="/departures/:id" element={<DepartureDetailPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/orders/:id" element={<OrderDetailPage />} />
        <Route path="/tour-templates" element={<TourTemplateListPage />} />
        <Route path="/tour-templates/:id" element={<TourTemplateDetailPage />} />
        <Route path="/billing" element={<BillingPage />} />
        <Route path="/reports/order-debt" element={<OrderDebtReportPage />} />
      </Route>
    </Routes>
  );
}
