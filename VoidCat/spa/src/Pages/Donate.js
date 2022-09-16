import {useState} from "react";

export function Donate() {
    const Hostname = "pay.v0l.io";
    const StoreId = "CxjchLEkirhBWU17KeJrAe71g5TzrxsvsfLuFwrnyp5Q";

    const [currency, setCurrency] = useState("USD");
    const [price, setPrice] = useState(1);

    return (
        <div className="page">
            <h2>Donate with Bitcoin</h2>
            <form method="POST" action={`https://${Hostname}/api/v1/invoices`} className="flex">
                <input type="hidden" name="storeId" value={StoreId}/>
                <input type="hidden" name="checkoutDesc" value="Donation"/>
                <div className="flex">
                    <input name="price" type="number" min="1" step="1" value={price}
                           onChange={(e) => setPrice(parseFloat(e.target.value))}/>
                    <select name="currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
                        <option>USD</option>
                        <option>GBP</option>
                        <option>EUR</option>
                        <option>BTC</option>
                    </select>
                </div>
                <input type="image"
                       name="submit"
                       src={`https://${Hostname}/img/paybutton/pay.svg`}
                       alt="Pay with BTCPay Server, a Self-Hosted Bitcoin Payment Processor"/>
            </form>
        </div>
    );
}