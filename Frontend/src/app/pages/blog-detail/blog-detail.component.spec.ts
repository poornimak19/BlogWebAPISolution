import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BlogDetailComponent } from './blog-detail.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('BlogDetailComponent', () => {
  let fixture: ComponentFixture<BlogDetailComponent>;
  let comp: BlogDetailComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BlogDetailComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(BlogDetailComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start loading', () => expect(comp.loading()).toBeTrue());
  it('should have null post initially', () => expect(comp.post()).toBeNull());
  it('should start unliked', () => expect(comp.liked()).toBeFalse());
  it('should start likeCount at 0', () => expect(comp.likeCount()).toBe(0));
  it('isAuthor should be false when not logged in', () => expect(comp.isAuthor()).toBeFalse());
  it('readTime should return at least 1', () => {
    comp.post.set({ id:'1',title:'T',slug:'t',contentHtml:'<p>Hello world test content</p>',status:'Published',visibility:'Public',commentsEnabled:true,autoApproveComments:true,createdAt:'',updatedAt:'',author:{id:'a',username:'u'},tags:[],categories:[],allowedAudienceUserIds:[] });
    expect(comp.readTime()).toBeGreaterThanOrEqual(1);
  });
  it('should show skeleton while loading', () => {
    comp.loading.set(true); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.sk-wrap')).toBeTruthy();
  });
  it('should show 404 when post is null and not loading', () => {
    comp.loading.set(false); comp.post.set(null); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.not-found')).toBeTruthy();
  });
});
