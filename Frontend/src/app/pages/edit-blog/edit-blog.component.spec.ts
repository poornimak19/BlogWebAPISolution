import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EditBlogComponent } from './edit-blog.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('EditBlogComponent', () => {
  let fixture: ComponentFixture<EditBlogComponent>;
  let comp: EditBlogComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditBlogComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(EditBlogComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start loading', () => expect(comp.loading()).toBeTrue());
  it('should start not saving', () => expect(comp.saving()).toBeFalse());
  it('should show loading message initially', () => { expect(fixture.nativeElement.querySelector('.edit-loading')).toBeTruthy(); });
  it('should toggle category', () => { comp.toggleCat('Tech'); expect(comp.form.categoryNames).toContain('Tech'); comp.toggleCat('Tech'); expect(comp.form.categoryNames).not.toContain('Tech'); });
  it('should add and remove tag', () => {
    comp.tagInput = 'vue'; comp.addTag(new KeyboardEvent('keydown'));
    expect(comp.form.tagNames).toContain('vue');
    comp.removeTag('vue');
    expect(comp.form.tagNames).not.toContain('vue');
  });
  it('should require title on save', () => { comp.post.set({ id:'1',title:'',slug:'s',contentHtml:'',status:'Draft',visibility:'Public',commentsEnabled:true,autoApproveComments:true,createdAt:'',updatedAt:'',author:{id:'a',username:'u'},tags:[],categories:[],allowedAudienceUserIds:[] }); comp.save('Draft'); expect(comp.saving()).toBeFalse(); });
  it('wordCount returns 0 for empty html', () => { comp.html.set(''); expect(comp.wordCount()).toBe(0); });
});
