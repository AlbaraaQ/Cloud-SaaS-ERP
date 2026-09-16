import { Module } from '@nestjs/common';

import { SalesModule } from '../sales/sales.module.js';

import { PaymentsController } from './payments.controller.js';
import { PaymentsService } from './payments.service.js';

@Module({
  // `SalesModule` for one reason: an approved 💳 is money in, and it lands on the invoice
  // through `SalesService.addPayment` — the same call `POST sales/invoices/:id/payments`
  // makes, with `method: 'card'`.
  imports: [SalesModule],
  controllers: [PaymentsController],
  providers: [PaymentsService],
  exports: [PaymentsService],
})
export class PaymentsModule {}
