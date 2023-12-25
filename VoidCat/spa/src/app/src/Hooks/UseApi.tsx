import { useSelector } from "react-redux";
import { VoidApi } from "@void-cat/api";

import { RootState } from "@/Store";

export default function useApi() {
  const auth = useSelector((s: RootState) => s.login.jwt);
  return new VoidApi(
    import.meta.env.VITE_API_HOST,
    auth ? () => Promise.resolve(`Bearer ${auth}`) : undefined,
  );
}
