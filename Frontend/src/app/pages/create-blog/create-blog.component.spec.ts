import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateBlogComponent } from './create-blog.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('CreateBlogComponent', () => {
  let fixture: ComponentFixture<CreateBlogComponent>;
  let comp: CreateBlogComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateBlogComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(CreateBlogComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start not saving', () => expect(comp.saving()).toBeFalse());
  it('should show error when title is empty', () => { comp.save('Draft'); expect(comp.saving()).toBeFalse(); });
  it('should add a tag', () => { comp.tagInput = 'angular'; comp.addTag(new KeyboardEvent('keydown')); expect(comp.form.tagNames).toContain('angular'); });
  it('should not add duplicate tag', () => { comp.form.tagNames = ['angular']; comp.tagInput = 'angular'; comp.addTag(new KeyboardEvent('keydown')); expect(comp.form.tagNames.filter(t => t === 'angular').length).toBe(1); });
  it('should remove a tag', () => { comp.form.tagNames = ['angular', 'testing']; comp.removeTag('angular'); expect(comp.form.tagNames).not.toContain('angular'); });
  it('should toggle category on/off', () => { comp.toggleCat('Tech'); expect(comp.form.categoryNames).toContain('Tech'); comp.toggleCat('Tech'); expect(comp.form.categoryNames).not.toContain('Tech'); });
  it('wordCount should return 0 when empty', () => { comp.html.set(''); expect(comp.wordCount()).toBe(0); });
  it('wordCount should count words in html', () => { comp.html.set('<p>Hello world foo</p>'); expect(comp.wordCount()).toBe(3); });
});
