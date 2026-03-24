import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentComponent } from './comment.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('CommentComponent', () => {
  let fixture: ComponentFixture<CommentComponent>;
  let comp: CommentComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(CommentComponent);
    comp    = fixture.componentInstance;
    comp.postId = 'test-post-id';
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start with empty threads', () => expect(comp.threads().length).toBe(0));
  it('should start total at 0', () => expect(comp.total()).toBe(0));
  it('should return ? avatar letter when not logged in', () => expect(comp.avatarLetter).toBe('?'));
  it('should not submit empty comment', () => { comp.newText = '  '; comp.submit(); expect(comp.submitting()).toBeFalse(); });
  it('should cancel compose correctly', () => {
    comp.newText = 'hello'; comp.composeFocused.set(true);
    comp.cancelCompose();
    expect(comp.newText).toBe(''); expect(comp.composeFocused()).toBeFalse();
  });
  it('isLiked returns false initially', () => expect(comp.isLiked('abc')).toBeFalse());
  it('isLiked returns true after adding', () => { comp.likedSet.add('abc'); expect(comp.isLiked('abc')).toBeTrue(); });
});
