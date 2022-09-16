import './App.css';

import {BrowserRouter, Routes, Route} from "react-router-dom";
import {Provider} from "react-redux";
import store from "./Store";
import {FilePreview} from "./Pages/FilePreview";
import {HomePage} from "./Pages/HomePage";
import {Admin} from "./Admin/Admin";
import {UserLogin} from "./Pages/UserLogin";
import {Profile} from "./Pages/Profile";
import {Header} from "./Components/Shared/Header";
import {Donate} from "./Pages/Donate";


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
                        <Route exact path="/donate" element={<Donate/>}/>
                    </Routes>
                </BrowserRouter>
            </Provider>
        </div>
    );
}

export default App;
