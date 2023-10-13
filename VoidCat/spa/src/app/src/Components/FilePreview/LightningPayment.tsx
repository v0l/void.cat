import QRCode from "qrcode.react";
import { useEffect } from "react";
import {
  PaymentOrder,
  PaymentOrderState,
  VoidFileResponse,
} from "@void-cat/api";

import { Countdown } from "../Shared/Countdown";

import useApi from "Hooks/UseApi";

interface LightningPaymentProps {
  file: VoidFileResponse;
  order: PaymentOrder;
  onPaid: (s: PaymentOrder) => void;
  onReset: () => void;
}

export function LightningPayment({
  file,
  order,
  onPaid,
  onReset,
}: LightningPaymentProps) {
  const Api = useApi();
  const link = `lightning:${order.orderLightning?.invoice}`;

  function openInvoice() {
    const a = document.createElement("a");
    a.href = link;
    a.click();
  }

  async function checkStatus() {
    const os = await Api.getOrder(file.id, order.id);
    if (os.status === PaymentOrderState.Paid && typeof onPaid === "function") {
      onPaid(os);
    }
  }

  useEffect(() => {
    let t = setInterval(checkStatus, 2500);
    return () => clearInterval(t);
  }, []);

  return (
    <div className="lightning-invoice" onClick={openInvoice}>
      <h1>Pay with Lightning ⚡</h1>
      <QRCode value={link} size={512} includeMargin={true} />
      <dl>
        <dt>Expires:</dt>
        <dd>
          <Countdown to={order.orderLightning!.expire} onEnded={onReset} />
        </dd>
      </dl>
    </div>
  );
}
