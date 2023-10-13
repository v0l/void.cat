import React from "react";
import { VoidButton } from "../Shared/VoidButton";
import {
  PaymentCurrencies,
  SetPaymentConfigRequest,
  VoidFileResponse,
} from "@void-cat/api";

interface NoPaymentConfigProps {
  privateFile: VoidFileResponse;
  onSaveConfig: (c: SetPaymentConfigRequest) => Promise<any>;
}

export function NoPaymentConfig({
  privateFile,
  onSaveConfig,
}: NoPaymentConfigProps) {
  async function saveConfig() {
    const cfg = {
      editSecret: privateFile.metadata!.editSecret,
      required: false,
      amount: 0,
      currency: PaymentCurrencies.BTC,
    } as SetPaymentConfigRequest;

    await onSaveConfig(cfg);
  }

  return (
    <div>
      <VoidButton onClick={saveConfig} options={{ showSuccess: true }}>
        Save
      </VoidButton>
    </div>
  );
}
