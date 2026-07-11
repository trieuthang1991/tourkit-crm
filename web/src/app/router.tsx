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
import { CustomerTypesPage } from '../features/customerTypes/CustomerTypesPage';
import { CustomerSourcesPage } from '../features/customerSources/CustomerSourcesPage';
import { CustomerTagsPage } from '../features/customerTags/CustomerTagsPage';
import { PaymentAccountsPage } from '../features/paymentAccounts/PaymentAccountsPage';
import { CarTypesPage } from '../features/carTypes/CarTypesPage';
import { LanguageTypesPage } from '../features/languageTypes/LanguageTypesPage';
import { DepartmentsPage } from '../features/departments/DepartmentsPage';
import { PositionsPage } from '../features/departments/PositionsPage';
import { UsersPage } from '../features/users/UsersPage';
import { SurchargesPage } from '../features/surcharges/SurchargesPage';
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
import { VehiclesPage } from '../features/vehicles/VehiclesPage';
import { GuideAssignmentsPage } from '../features/guides/GuideAssignmentsPage';
import { VehicleAssignmentsPage } from '../features/vehicleAssignments/VehicleAssignmentsPage';
import { ActivityLogsPage } from '../features/activityLogs/ActivityLogsPage';
import { OperationsCalendarPage } from '../features/operations/OperationsCalendarPage';
import { ServiceBookingsPage } from '../features/serviceBookings/ServiceBookingsPage';
import { AgentsPage } from '../features/agents/AgentsPage';
import { CustomerCommissionRulesPage } from '../features/customerCommissionRules/CustomerCommissionRulesPage';
import { QuotesPage } from '../features/quotes/QuotesPage';
import { QuotePrintPage } from '../features/quotes/QuotePrintPage';
import { InvoicesPage } from '../features/invoices/InvoicesPage';
import { AgentQuotesPage } from '../features/agentQuotes/AgentQuotesPage';
import { TicketFundsPage } from '../features/ticketFunds/TicketFundsPage';
import { AgentBookingsPage } from '../features/agentBookings/AgentBookingsPage';

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegistrationPage />} />
      {/* Bản in báo giá: cần đăng nhập nhưng nằm NGOÀI AppShell để trang in sạch (không sidebar). */}
      <Route
        path="/quotes/:id/print"
        element={
          <ProtectedRoute>
            <QuotePrintPage />
          </ProtectedRoute>
        }
      />
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
        <Route path="/customer-types" element={<CustomerTypesPage />} />
        <Route path="/customer-sources" element={<CustomerSourcesPage />} />
        <Route path="/customer-tags" element={<CustomerTagsPage />} />
        <Route path="/payment-accounts" element={<PaymentAccountsPage />} />
        <Route path="/car-types" element={<CarTypesPage />} />
        <Route path="/language-types" element={<LanguageTypesPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/departments" element={<DepartmentsPage />} />
        <Route path="/positions" element={<PositionsPage />} />
        <Route path="/surcharges" element={<SurchargesPage />} />
        <Route path="/departures" element={<DeparturesPage />} />
        <Route path="/departures/:id" element={<DepartureDetailPage />} />
        <Route path="/operations-calendar" element={<OperationsCalendarPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/orders/:id" element={<OrderDetailPage />} />
        <Route path="/service-bookings" element={<ServiceBookingsPage />} />
        <Route path="/agents" element={<AgentsPage />} />
        <Route path="/customer-commission-rules" element={<CustomerCommissionRulesPage />} />
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
        <Route path="/vehicles" element={<VehiclesPage />} />
        <Route path="/guide-assignments" element={<GuideAssignmentsPage />} />
        <Route path="/vehicle-assignments" element={<VehicleAssignmentsPage />} />
        <Route path="/activity-logs" element={<ActivityLogsPage />} />
        <Route path="/quotes" element={<QuotesPage />} />
        <Route path="/invoices" element={<InvoicesPage />} />
        <Route path="/agent-quotes" element={<AgentQuotesPage />} />
        <Route path="/ticket-funds" element={<TicketFundsPage />} />
        <Route path="/agent-bookings" element={<AgentBookingsPage />} />
      </Route>
    </Routes>
  );
}
