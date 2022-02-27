import {BrowserRouter, Routes, Route} from "react-router-dom";
import {Provider} from "react-redux";
import store from "./Store";
import {FilePreview} from "./FilePreview";
import {HomePage} from "./HomePage";
import {Admin} from "./Admin/Admin";
import {UserLogin} from "./UserLogin";
import {Profile} from "./Profile";
import {Header} from "./Header";

import './App.css';

function App() {
    return (
        <div className="app">
            <Provider store={store}>
                <BrowserRouter>
                    <Header/>
                    <Routes>
                        <Route exact path="/" element={<HomePage/>}/>
                        <Route exact path="/login" element={<UserLogin/>}/>
                        <Route exact path="/u/:id" element={<Profile/>}/>
                        <Route exact path="/admin" element={<Admin/>}/>
                        <Route exact path="/:id" element={<FilePreview/>}/>
                    </Routes>
                </BrowserRouter>
            </Provider>
        </div>
    );
}

export default App;
