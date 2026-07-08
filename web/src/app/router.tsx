import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { AppShell } from './AppShell';
import { TourTemplateListPage } from '../features/tourTemplates/TourTemplateListPage';

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
        <Route path="/" element={<Navigate to="/tour-templates" replace />} />
        <Route path="/tour-templates" element={<TourTemplateListPage />} />
      </Route>
    </Routes>
  );
}
