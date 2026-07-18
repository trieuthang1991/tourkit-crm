import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';

/* Bắt lỗi render của 1 màn → hiển thị thông báo thân thiện, GIỮ khung app (sidebar/header),
   thay vì để 1 lỗi undefined làm trắng toàn bộ ứng dụng. Reset bằng key={pathname} khi đổi route. */

export class ErrorBoundary extends Component<{ children: ReactNode }, { error: Error | null }> {
  state: { error: Error | null } = { error: null };

  static getDerivedStateFromError(error: Error) {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Màn hình gặp lỗi:', error, info.componentStack);
  }

  render() {
    if (this.state.error) {
      return (
        <div style={{ padding: 40, textAlign: 'center' }}>
          <div style={{ font: '700 18px var(--tk-font)', color: 'var(--tk-heading)', marginBottom: 8 }}>
            Màn hình gặp sự cố
          </div>
          <div style={{ color: 'var(--tk-muted)', marginBottom: 18 }}>
            Đã có lỗi khi hiển thị màn này. Bạn thử tải lại hoặc chuyển sang màn khác.
          </div>
          <button type="button" className="rf-btn rf-btn--primary" onClick={() => this.setState({ error: null })}>
            Thử lại
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
