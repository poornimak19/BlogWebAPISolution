import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PostCardComponent } from './post-card.component';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { PostSummaryDto } from '../../models/post.models';

const MOCK_POST: PostSummaryDto = {
  id: 'p1', title: 'Hello World', slug: 'hello-world',
  excerpt: 'A short excerpt.', coverImageUrl: '',
  status: 'Published', visibility: 'Public',
  publishedAt: '2025-01-01T00:00:00Z',
  createdAt:   '2025-01-01T00:00:00Z',
  updatedAt:   '2025-01-01T00:00:00Z',
  author: { id: 'u1', username: 'alice', displayName: 'Alice' },
  tags: ['angular', 'testing'], categories: ['Tech']
};

describe('PostCardComponent', () => {
  let fixture: ComponentFixture<PostCardComponent>;
  let comp: PostCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostCardComponent, RouterTestingModule, HttpClientTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(PostCardComponent);
    comp    = fixture.componentInstance;
    comp.post = MOCK_POST;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should display post title', () => expect(fixture.nativeElement.querySelector('.card-title').textContent).toContain('Hello World'));
  it('should display excerpt', () => expect(fixture.nativeElement.querySelector('.card-excerpt').textContent).toContain('A short excerpt'));
  it('should display author', () => expect(fixture.nativeElement.querySelector('.card-author').textContent).toContain('Alice'));
  it('should show tags', () => expect(fixture.nativeElement.querySelectorAll('.card-tag').length).toBe(2));
  it('should start unliked', () => expect(comp.liked()).toBeFalse());
  it('should start with 0 likes', () => expect(comp.likeCount()).toBe(0));
  it('should not show cover when empty', () => expect(fixture.nativeElement.querySelector('.card-cover')).toBeNull());
  it('should show cover when URL is set', () => {
    comp.post = { ...MOCK_POST, coverImageUrl: 'https://example.com/img.jpg' };
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.card-cover')).toBeTruthy();
  });
});
