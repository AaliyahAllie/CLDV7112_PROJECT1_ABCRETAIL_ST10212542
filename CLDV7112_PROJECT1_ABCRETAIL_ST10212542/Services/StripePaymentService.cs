using Stripe;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services
{
    public class StripePaymentService
    {
        // Creates a PaymentIntent and returns the client_secret for the browser
        public async Task<PaymentIntent> CreatePaymentIntentAsync(long amountInCents, string currency = "zar")
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                }
            };
            var service = new PaymentIntentService();
            return await service.CreateAsync(options);
        }

        // Retrieves a PaymentIntent so the server can verify the payment succeeded
        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            return await service.GetAsync(paymentIntentId);
        }
    }
}
