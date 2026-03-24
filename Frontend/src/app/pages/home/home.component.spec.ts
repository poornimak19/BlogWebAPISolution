import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeComponent } from './home.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('HomeComponent', () => {
  let fixture: ComponentFixture<HomeComponent>;
  let comp: HomeComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(HomeComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start page at 1', () => expect(comp.page()).toBe(1));
  it('should start with no active tag', () => expect(comp.activeTag()).toBeNull());
  it('should start sort as latest', () => expect(comp.sort()).toBe('latest'));
  it('filterByTag should set tag and reset page', () => { comp.page.set(3); comp.filterByTag('angular'); expect(comp.activeTag()).toBe('angular'); expect(comp.page()).toBe(1); });
  it('setSort should update sort signal', () => { comp.setSort('popular'); expect(comp.sort()).toBe('popular'); });
  it('totalPages computed correctly', () => { comp.total.set(27); expect(comp.totalPages()).toBe(3); });
  it('pageNumbers should be windowed', () => { comp.total.set(90); comp.page.set(5); expect(comp.pageNumbers().length).toBeLessThanOrEqual(5); });
});
