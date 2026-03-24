import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SearchComponent } from './search.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('SearchComponent', () => {
  let fixture: ComponentFixture<SearchComponent>;
  let comp: SearchComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(SearchComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start page at 1', () => expect(comp.page()).toBe(1));
  it('should start with no selected tag', () => expect(comp.selectedTag()).toBeNull());
  it('should start with no selected cat', () => expect(comp.selectedCat()).toBeNull());
  it('should not have searched initially', () => expect(comp.hasSearched()).toBeFalse());
  it('setTag should update selectedTag and reset page', () => { comp.page.set(3); comp.setTag('angular'); expect(comp.selectedTag()).toBe('angular'); expect(comp.page()).toBe(1); });
  it('setCat should update selectedCat', () => { comp.setCat('tech'); expect(comp.selectedCat()).toBe('tech'); });
  it('reset should clear all filters', () => { comp.query = 'test'; comp.selectedTag.set('a'); comp.selectedCat.set('b'); comp.reset(); expect(comp.query).toBe(''); expect(comp.selectedTag()).toBeNull(); expect(comp.selectedCat()).toBeNull(); });
  it('totalPages computed correctly', () => { comp.total.set(27); expect(comp.totalPages()).toBe(3); });
});
