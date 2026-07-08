import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { AppShell } from './AppShell';
import { TourTemplateListPage } from '../features/tourTemplates/TourTemplateListPage';
import { CustomersPage } from '../features/customers/CustomersPage';

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
        <Route path="/tour-templates" element={<TourTemplateListPage />} />
      </Route>
    </Routes>
  );
}
