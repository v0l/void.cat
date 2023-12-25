import "./App.css";

import {
  createBrowserRouter,
  LoaderFunctionArgs,
  Outlet,
  RouterProvider,
} from "react-router-dom";
import { Provider } from "react-redux";
import { VoidApi } from "@void-cat/api";

import store from "./Store";
import { FilePreview } from "./Pages/FilePreview";
import { HomePage } from "./Pages/HomePage";
import { Admin } from "./Admin/Admin";
import { UserLogin } from "./Pages/UserLogin";
import { ProfilePage } from "./Pages/Profile";
import { Header } from "./Components/Shared/Header";
import { Donate } from "./Pages/Donate";

const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      {
        path: "/",
        element: <HomePage />,
      },
      {
        path: "/login",
        element: <UserLogin />,
      },
      {
        path: "/u/:id",
        loader: async ({ params }: LoaderFunctionArgs) => {
          const state = store.getState();
          const api = new VoidApi(
            import.meta.env.VITE_API_HOST,
            state.login.jwt
              ? () => Promise.resolve(`Bearer ${state.login.jwt}`)
              : undefined,
          );
          if (params.id) {
            try {
              return await api.getUser(params.id);
            } catch (e) {
              console.error(e);
            }
          }
          return null;
        },
        element: <ProfilePage />,
      },
      {
        path: "/admin",
        element: <Admin />,
      },
      {
        path: "/:id",
        element: <FilePreview />,
      },
      {
        path: "/donate",
        element: <Donate />,
      },
    ],
  },
]);

export function AppLayout() {
  return (
    <div className="app">
      <Provider store={store}>
        <Header />
        <Outlet />
      </Provider>
    </div>
  );
}

export default function App() {
  return <RouterProvider router={router} />;
}
