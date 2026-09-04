import { Injectable } from '@nestjs/common';

import { getDb } from '@erp/database';

@Injectable()
export class DatabaseService {
  async checkConnection(): Promise<boolean> {
    try {
      const db = getDb();
      await db.execute('SELECT 1');
      return true;
    } catch {
      return false;
    }
  }
}
