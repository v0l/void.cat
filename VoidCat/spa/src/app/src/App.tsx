import './App.css';

import {createBrowserRouter, LoaderFunctionArgs, Outlet, RouterProvider} from "react-router-dom";
import {Provider} from "react-redux";

import store from "./Store";
import {FilePreview} from "./Pages/FilePreview";
import {HomePage} from "./Pages/HomePage";
import {Admin} from "./Admin/Admin";
import {UserLogin} from "./Pages/UserLogin";
import {ProfilePage} from "./Pages/Profile";
import {Header} from "./Components/Shared/Header";
import {Donate} from "./Pages/Donate";
import {VoidApi} from "@void-cat/api";
import {ApiHost} from "./Const";


const router = createBrowserRouter([
    {
        element: <AppLayout/>,
        children: [
            {
                path: "/",
                element: <HomePage/>
            },
            {
                path: "/login",
                element: <UserLogin/>
            },
            {
                path: "/u/:id",
                loader: async ({params}: LoaderFunctionArgs) => {
                    const api = new VoidApi(ApiHost, store.getState().login.jwt);
                    if(params.id) {
                        return await api.getUser(params.id);
                    }
                    return null;
                },
                element: <ProfilePage/>
            },
            {
                path: "/admin",
                element: <Admin/>
            },
            {
                path: "/:id",
                element: <FilePreview/>
            },
            {
                path: "/donate",
                element: <Donate/>
            }
        ]
    }
])

export function AppLayout() {
    return (
        <div className="app">
            <Provider store={store}>
                <Header/>
                <Outlet/>
            </Provider>
        </div>
    );
}

export default function App() {
    return <RouterProvider router={router}/>
}
