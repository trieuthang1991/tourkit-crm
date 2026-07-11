import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { AppShell } from './AppShell';
import { TourTemplateListPage } from '../features/tourTemplates/TourTemplateListPage';
import { TourTemplateDetailPage } from '../features/tourTemplates/TourTemplateDetailPage';
import { CustomersPage } from '../features/customers/CustomersPage';
import { LeadsPage } from '../features/leads/LeadsPage';
import { ProvidersPage } from '../features/providers/ProvidersPage';
import { ServiceItemsPage } from '../features/services/ServiceItemsPage';
import { ProviderServicesPage } from '../features/services/ProviderServicesPage';
import { MarketingPage } from '../features/marketing/MarketingPage';
import { MarketTypesPage } from '../features/marketTypes/MarketTypesPage';
import { DeparturesPage } from '../features/booking/DeparturesPage';
import { DepartureDetailPage } from '../features/booking/DepartureDetailPage';
import { OrdersPage } from '../features/booking/OrdersPage';
import { OrderDetailPage } from '../features/booking/OrderDetailPage';
import { BillingPage } from '../features/billing/BillingPage';
import { OrderDebtReportPage } from '../features/reports/OrderDebtReportPage';
import { ProviderDebtReportPage } from '../features/reports/ProviderDebtReportPage';
import { DashboardPage } from '../features/reports/DashboardPage';
import { CashFlowReportPage } from '../features/reports/CashFlowReportPage';
import { TurnoverReportPage } from '../features/reports/TurnoverReportPage';
import { CommissionByUserReportPage } from '../features/reports/CommissionByUserReportPage';
import { CommissionRulesPage } from '../features/commission/CommissionRulesPage';
import { RegistrationPage } from '../features/registration/RegistrationPage';
import { CustomerCaresPage } from '../features/care/CustomerCaresPage';
import { TourRatingsPage } from '../features/ratings/TourRatingsPage';
import { QuotesPage } from '../features/quotes/QuotesPage';

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegistrationPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/customers" element={<CustomersPage />} />
        <Route path="/leads" element={<LeadsPage />} />
        <Route path="/providers" element={<ProvidersPage />} />
        <Route path="/service-items" element={<ServiceItemsPage />} />
        <Route path="/provider-services" element={<ProviderServicesPage />} />
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
        <Route path="/reports/provider-debt" element={<ProviderDebtReportPage />} />
        <Route path="/reports/cash-flow" element={<CashFlowReportPage />} />
        <Route path="/reports/turnover" element={<TurnoverReportPage />} />
        <Route path="/reports/commission-by-user" element={<CommissionByUserReportPage />} />
        <Route path="/commission-rules" element={<CommissionRulesPage />} />
        <Route path="/customer-cares" element={<CustomerCaresPage />} />
        <Route path="/tour-ratings" element={<TourRatingsPage />} />
        <Route path="/quotes" element={<QuotesPage />} />
      </Route>
    </Routes>
  );
}
