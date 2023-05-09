import {useSelector} from "react-redux";

import {RootState} from "Store";
import {VoidApi} from "Api";
import {ApiHost} from "Const";

export default function useApi() {
    const auth = useSelector((s: RootState) => s.login.jwt);
    return new VoidApi(ApiHost, auth);
}