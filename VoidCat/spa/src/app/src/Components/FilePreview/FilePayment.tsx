import "./FilePayment.css";
import { useState } from "react";
import { LightningPayment } from "./LightningPayment";
import { VoidButton } from "../Shared/VoidButton";
import { PaymentOrder, PaymentServices, VoidFileResponse } from "@void-cat/api";

import useApi from "@/Hooks/UseApi";
import { FormatCurrency } from "@/Util";

interface FilePaymentProps {
  file: VoidFileResponse;
  onPaid: () => Promise<void>;
}

export function FilePayment({ file, onPaid }: FilePaymentProps) {
  const Api = useApi();
  const paymentKey = `payment-${file.id}`;
  const [order, setOrder] = useState<any>();

  // Payment not required
  if (!file.payment) return null;

  async function fetchOrder() {
    try {
      const rsp = await Api.createOrder(file.id);
      setOrder(rsp);
    } catch (e) {
      console.error(e);
    }
  }

  function reset() {
    setOrder(undefined);
  }

  function handlePaid(order: PaymentOrder) {
    window.localStorage.setItem(paymentKey, JSON.stringify(order));
    if (typeof onPaid === "function") {
      onPaid();
    }
  }

  if (!order) {
    const amountString = FormatCurrency(
      file.payment.amount,
      file.payment.currency,
    );
    if (file.payment.required) {
      return (
        <div className="payment">
          <h3>You must pay {amountString} to view this file.</h3>
          <VoidButton onClick={fetchOrder}>Pay</VoidButton>
        </div>
      );
    } else {
      return <VoidButton onClick={fetchOrder}>Tip {amountString}</VoidButton>;
    }
  } else {
    switch (file.payment.service) {
      case PaymentServices.Strike: {
        return (
          <LightningPayment
            file={file}
            order={order}
            onReset={reset}
            onPaid={handlePaid}
          />
        );
      }
    }
  }
  return null;
}
