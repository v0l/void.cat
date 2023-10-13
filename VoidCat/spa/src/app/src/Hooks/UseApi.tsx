import { useSelector } from "react-redux";
import { VoidApi } from "@void-cat/api";

import { RootState } from "Store";
import { ApiHost } from "Const";

export default function useApi() {
  const auth = useSelector((s: RootState) => s.login.jwt);
  return new VoidApi(ApiHost, auth ? () => Promise.resolve(`Bearer ${auth}`) : undefined);
}
