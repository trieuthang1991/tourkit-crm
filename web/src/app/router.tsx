import { Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { TourTemplateListPage } from '../features/tourTemplates/TourTemplateListPage';

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <TourTemplateListPage />
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}
