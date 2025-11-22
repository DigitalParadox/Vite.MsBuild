import React from 'react';
import ReactDOM from 'react-dom/client';

export function mount(props: any) {
  const root = ReactDOM.createRoot(props.container);
  root.render(<div>Customer App</div>);
}

export function unmount(props: any) {
  // Cleanup
}
