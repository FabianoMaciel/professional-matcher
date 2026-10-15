import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Country, MarketInsight } from '../../models/market.model';

@Injectable({ providedIn: 'root' })
export class MarketService {
  private readonly http = inject(HttpClient);

  countries(): Observable<Country[]> {
    return this.http.get<Country[]>(`${environment.apiBaseUrl}/market/countries`);
  }

  insights(
    professionalId: string,
    countryCodes: string[],
    resumeId: string | null,
  ): Observable<MarketInsight[]> {
    let params = new HttpParams().set('countries', countryCodes.join(','));
    if (resumeId) {
      params = params.set('resumeId', resumeId);
    }
    return this.http.get<MarketInsight[]>(
      `${environment.apiBaseUrl}/professionals/${professionalId}/market-insights`,
      { params },
    );
  }
}
