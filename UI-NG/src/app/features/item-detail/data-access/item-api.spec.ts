import { Component, inject, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, authResponse, envelope } from '../../../../testing/test-providers';
import { apiUrl } from '../../../core/api/api-response';
import { toSession } from '../../../core/auth/auth.models';
import { ItemApi } from './item-api';

@Component({ template: '' })
class Host {
  readonly itemType = signal(1);
  readonly items = inject(ItemApi).listForCurrentUser(this.itemType);
}

describe('ItemApi', () => {
  let http: HttpTestingController;
  const userId = authResponse().userId;

  afterEach(() => http.verify());

  function setup(signedIn: boolean) {
    sessionStorage.clear();
    if (signedIn) {
      sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse())));
    }
    TestBed.configureTestingModule({ providers: appTestProviders() });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    TestBed.tick();
    return fixture.componentInstance;
  }

  it("requests the signed-in user's items of the given type", () => {
    const host = setup(true);

    http.expectOne(apiUrl(`/items/${userId}/1`)).flush(envelope([]));

    host.itemType.set(2);
    TestBed.tick();
    http.expectOne(apiUrl(`/items/${userId}/2`)).flush(envelope([]));
  });

  it('sends nothing when nobody is signed in', () => {
    setup(false);

    http.expectNone(() => true);
  });
});
