using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.VisualTree;
using ReactiveUI;
using WalletWasabi.Fluent.Controls;
using WalletWasabi.Fluent.Models;

namespace WalletWasabi.Fluent.Behaviors;

public class FadePocketLabelsBehavior : AttachedToVisualTreeBehavior<TagsBox>
{
	private TagControl[] _currentTags = Array.Empty<TagControl>();
	private CompositeDisposable? _disposable;

	public static readonly StyledProperty<Pocket[]> PocketsProperty =
		AvaloniaProperty.Register<FadePocketLabelsBehavior, Pocket[]>(nameof(Pockets));

	public Pocket[] Pockets
	{
		get => GetValue(PocketsProperty);
		set => SetValue(PocketsProperty, value);
	}

	protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
	{
		if (AssociatedObject is null)
		{
			return;
		}

		Observable
			.FromEventPattern(AssociatedObject, nameof(AssociatedObject.LayoutUpdated))
			.Subscribe(_ =>
			{
				var tagItems = AssociatedObject.GetVisualDescendants().OfType<TagControl>().ToArray();

				if (tagItems.Any() && tagItems.Length != _currentTags.Length)
				{
					_currentTags = tagItems;
					_disposable?.Dispose();
					_disposable = new CompositeDisposable();

					foreach (var tagControl in _currentTags)
					{
						tagControl
							.WhenAnyValue(x => x.IsPointerOver)
							.Skip(1)
							.Subscribe(x =>
							{
								var tagControlLabel = tagControl.DataContext;
								var affectedPockets = Pockets.Where(x => x.Labels.Contains(tagControlLabel));
								var remainingPockets = Pockets.Except(affectedPockets);
								var tagsToFade = _currentTags.Where(x => !remainingPockets.Any(y => y.Labels.Contains(x.DataContext)));

								foreach (var control in tagsToFade)
								{
									control.Opacity = x ? 0.3 : 1;
								}
							})
							.DisposeWith(_disposable);
					}
				}
			})
			.DisposeWith(disposable);
	}
}
