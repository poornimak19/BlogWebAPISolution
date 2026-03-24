import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('ProfileComponent', () => {
  let fixture: ComponentFixture<ProfileComponent>;
  let comp: ProfileComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfileComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(ProfileComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should start loading', () => expect(comp.loading()).toBeTrue());
  it('should start on posts tab', () => expect(comp.activeTab()).toBe('posts'));
  it('should start not following', () => expect(comp.isFollowing()).toBeFalse());
  it('avatarLetter should be ? when no profile', () => expect(comp.avatarLetter()).toBe('?'));
  it('isOwnProfile should be false without user', () => expect(comp.isOwnProfile()).toBeFalse());
  it('should open edit mode', () => { comp.profile.set({ id:'1',username:'alice',followers:0,following:0 }); comp.openEdit(); expect(comp.editMode()).toBeTrue(); });
  it('should close edit mode', () => { comp.editMode.set(true); comp.editMode.set(false); expect(comp.editMode()).toBeFalse(); });
  it('publishedCount should be 0 initially', () => expect(comp.publishedCount()).toBe(0));
});
